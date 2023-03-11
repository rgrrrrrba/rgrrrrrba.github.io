FROM jekyll/jekyll:latest
RUN gem install bundler jekyll webrick liquid
WORKDIR /srv/jekyll
EXPOSE 4000
CMD bundle exec jekyll serve --force_polling --host 0.0.0.0 --trace